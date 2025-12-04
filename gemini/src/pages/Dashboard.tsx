import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { campaignService } from '../services/api';
import { CampaignStatus, CampaignSummary } from '../types';
import { Plus, BarChart2, CheckCircle, XCircle, Clock } from 'lucide-react';

const Dashboard: React.FC = () => {
  const [campaigns, setCampaigns] = useState<CampaignSummary[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadCampaigns();
  }, []);

  const loadCampaigns = async () => {
    try {
      // In a real app, this might be paginated
      const data = await campaignService.getAll();
      setCampaigns(data);
    } catch (error) {
      console.error('Failed to load campaigns', error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: CampaignStatus) => {
    switch (status) {
      case CampaignStatus.Draft: return 'bg-gray-100 text-gray-600';
      case CampaignStatus.Sending: return 'bg-blue-100 text-blue-600';
      case CampaignStatus.Completed: return 'bg-green-100 text-green-600';
      case CampaignStatus.Failed: return 'bg-red-100 text-red-600';
      default: return 'bg-gray-100 text-gray-600';
    }
  };

  if (loading) return <div className="text-center py-20 text-gray-500">Loading dashboard...</div>;

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
          <p className="text-gray-500">Overview of your email campaigns</p>
        </div>
        <Link
          to="/campaigns/new"
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors shadow-sm"
        >
          <Plus size={18} />
          <span>Create Campaign</span>
        </Link>
      </div>

      {/* Stats Cards (Mocked aggregated data for visual) */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-white p-6 rounded-xl border border-gray-200 shadow-sm flex items-center gap-4">
           <div className="p-3 bg-blue-50 text-blue-600 rounded-lg">
             <BarChart2 size={24} />
           </div>
           <div>
             <p className="text-sm font-medium text-gray-500">Total Campaigns</p>
             <p className="text-2xl font-bold text-gray-900">{campaigns.length}</p>
           </div>
        </div>
        <div className="bg-white p-6 rounded-xl border border-gray-200 shadow-sm flex items-center gap-4">
           <div className="p-3 bg-green-50 text-green-600 rounded-lg">
             <CheckCircle size={24} />
           </div>
           <div>
             <p className="text-sm font-medium text-gray-500">Completed</p>
             <p className="text-2xl font-bold text-gray-900">
               {campaigns.filter(c => c.status === CampaignStatus.Completed).length}
             </p>
           </div>
        </div>
        <div className="bg-white p-6 rounded-xl border border-gray-200 shadow-sm flex items-center gap-4">
           <div className="p-3 bg-red-50 text-red-600 rounded-lg">
             <XCircle size={24} />
           </div>
           <div>
             <p className="text-sm font-medium text-gray-500">Failed</p>
             <p className="text-2xl font-bold text-gray-900">
               {campaigns.filter(c => c.status === CampaignStatus.Failed).length}
             </p>
           </div>
        </div>
      </div>

      {/* Campaign List */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-100 font-semibold text-gray-800">
          Recent Campaigns
        </div>
        {campaigns.length === 0 ? (
          <div className="p-8 text-center text-gray-500">
            No campaigns found. Create your first one!
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-gray-600">
              <thead className="bg-gray-50 text-gray-500 uppercase font-medium text-xs">
                <tr>
                  <th className="px-6 py-3">Campaign Name</th>
                  <th className="px-6 py-3">Status</th>
                  <th className="px-6 py-3 text-right">Recipients</th>
                  <th className="px-6 py-3 text-right">Sent</th>
                  <th className="px-6 py-3 text-right">Failed</th>
                  <th className="px-6 py-3">Date</th>
                  <th className="px-6 py-3"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {campaigns.map((campaign) => (
                  <tr key={campaign.id} className="hover:bg-gray-50 transition-colors">
                    <td className="px-6 py-4 font-medium text-gray-900">
                      <Link to={`/campaigns/${campaign.id}`} className="hover:text-blue-600 hover:underline">
                        {campaign.name}
                      </Link>
                      <div className="text-xs text-gray-400 font-normal">{campaign.subject}</div>
                    </td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor(campaign.status)}`}>
                        {campaign.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">{campaign.totalRecipients}</td>
                    <td className="px-6 py-4 text-right text-green-600">{campaign.sentCount}</td>
                    <td className="px-6 py-4 text-right text-red-600">{campaign.failedCount}</td>
                    <td className="px-6 py-4 text-gray-400">
                      <div className="flex items-center gap-1">
                        <Clock size={14} />
                        {new Date(campaign.createdAt).toLocaleDateString()}
                      </div>
                    </td>
                    <td className="px-6 py-4 text-right">
                       <Link to={`/campaigns/${campaign.id}`} className="text-blue-600 hover:text-blue-800 font-medium text-xs">
                         View
                       </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default Dashboard;
